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
                        docker volume create prometheus-config 2>/dev/null || true
                        cat prometheus/prometheus.yml | docker run --rm -i \
                            -v prometheus-config:/config \
                            alpine sh -c "cat > /config/prometheus.yml && chmod 644 /config/prometheus.yml && ls -la /config/ && echo '--- File content ---' && cat /config/prometheus.yml"
                        docker-compose up -d rabbitmq message-sender message-receiver prometheus grafana
                    '''
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

