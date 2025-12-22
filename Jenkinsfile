pipeline {
    agent any

    environment {
        DOCKER_COMPOSE = 'docker-compose'
        WORKSPACE_DIR = "${WORKSPACE}"
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build Docker Images') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        docker-compose build message-sender message-receiver
                    '''
                }
            }
        }

        stage('Stop Existing Containers') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        docker-compose stop rabbitmq message-sender message-receiver prometheus grafana || true
                        docker-compose rm -f rabbitmq message-sender message-receiver prometheus grafana || true
                    '''
                }
            }
        }

        stage('Start Services') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        # Ensure prometheus config file exists and is a file
                        echo "Checking prometheus config file..."
                        ls -la prometheus/ || echo "prometheus directory not found"
                        if [ ! -f prometheus/prometheus.yml ]; then
                            echo "Error: prometheus/prometheus.yml not found or not a file"
                            file prometheus/prometheus.yml || true
                            exit 1
                        fi
                        if [ -d prometheus/prometheus.yml ]; then
                            echo "Error: prometheus/prometheus.yml is a directory, not a file"
                            exit 1
                        fi
                        echo "Prometheus config file exists and is a file"
                        # Create prometheus config volume
                        docker volume create prometheus-config 2>/dev/null || true
                        # Copy config file content directly to volume using cat
                        echo "Copying prometheus config to volume..."
                        docker run --rm \
                            -v prometheus-config:/config \
                            -v ${WORKSPACE}/prometheus/prometheus.yml:/source.yml:ro \
                            alpine sh -c "cat /source.yml > /config/prometheus.yml && chmod 644 /config/prometheus.yml && ls -la /config/ && cat /config/prometheus.yml"
                        # Start services
                        docker-compose up -d rabbitmq message-sender message-receiver prometheus grafana
                    '''
                }
            }
        }

        stage('Health Check') {
            steps {
                script {
                    sleep(time: 30, unit: 'SECONDS')
                    dir("${WORKSPACE}") {
                        sh '''
                            echo "Checking services health..."
                            curl -f http://message-sender:8080/swagger || exit 1
                            curl -f http://message-receiver:8080/swagger || exit 1
                            curl -f http://prometheus:9090/-/healthy || exit 1
                            curl -f http://grafana:3000/api/health || exit 1
                            echo "All services are healthy!"
                        '''
                    }
                }
            }
        }
    }

    post {
        always {
            echo 'Pipeline completed'
        }
        success {
            echo 'Pipeline succeeded!'
        }
        failure {
            echo 'Pipeline failed!'
            dir("${WORKSPACE}") {
                sh 'docker-compose logs'
            }
        }
    }
}

