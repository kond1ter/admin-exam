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
                        # Ensure prometheus config file exists
                        if [ ! -f prometheus/prometheus.yml ]; then
                            echo "Error: prometheus/prometheus.yml not found"
                            ls -la prometheus/ || echo "prometheus directory not found"
                            exit 1
                        fi
                        # Verify it's a file, not a directory
                        if [ -d prometheus/prometheus.yml ]; then
                            echo "Error: prometheus/prometheus.yml is a directory, not a file"
                            exit 1
                        fi
                        # Create prometheus config volume and copy config file
                        docker volume create prometheus-config 2>/dev/null || true
                        # Create a temporary container to copy the config file
                        # Use absolute path and ensure we're copying a file
                        docker run --rm \
                            -v prometheus-config:/config \
                            -v ${WORKSPACE}:/workspace \
                            alpine sh -c "cp /workspace/prometheus/prometheus.yml /config/prometheus.yml && ls -la /config/"
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

